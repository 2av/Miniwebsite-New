export function SharebizzMaintenancePage() {
    return (
        <main className="sharebizz-maintenance">
            <header className="sharebizz-maintenance__header">
                <a className="sharebizz-wordmark" href="/" aria-label="ShareBizz home">
                    <span className="sharebizz-wordmark__mark" aria-hidden="true">
                        <span />
                        <span />
                        <span />
                    </span>
                    <span>share<span className="sharebizz-wordmark__accent">bizz</span></span>
                </a>
                <span className="sharebizz-maintenance__status">
                    <span aria-hidden="true" /> Under construction
                </span>
            </header>

            <section className="sharebizz-maintenance__content" aria-labelledby="sharebizz-title">
                <div className="sharebizz-maintenance__copy">
                    <p className="sharebizz-maintenance__eyebrow">A new way to grow is on its way</p>
                    <h1 id="sharebizz-title">We’re building something <span>good.</span></h1>
                    <p className="sharebizz-maintenance__message">
                        ShareBizz is getting ready to make its debut. We’re putting the finishing touches in place and will be
                        back online soon.
                    </p>
                    <div className="sharebizz-maintenance__note">
                        <span className="sharebizz-maintenance__note-icon" aria-hidden="true">✳</span>
                        Thanks for stopping by. Come back and see us soon.
                    </div>
                </div>

                <div className="sharebizz-build" aria-hidden="true">
                    <div className="sharebizz-build__label">IN THE WORKS</div>
                    <div className="sharebizz-build__scene">
                        <div className="sharebizz-build__sun" />
                        <div className="sharebizz-build__building">
                            <div className="sharebizz-build__roof" />
                            <div className="sharebizz-build__floor sharebizz-build__floor--top"><i /><i /></div>
                            <div className="sharebizz-build__floor sharebizz-build__floor--middle"><i /><i /><i /></div>
                            <div className="sharebizz-build__floor sharebizz-build__floor--base"><i /><i /><i /></div>
                        </div>
                        <div className="sharebizz-build__scaffold sharebizz-build__scaffold--left" />
                        <div className="sharebizz-build__scaffold sharebizz-build__scaffold--right" />
                        <div className="sharebizz-build__ground" />
                        <div className="sharebizz-build__spark sharebizz-build__spark--one">+</div>
                        <div className="sharebizz-build__spark sharebizz-build__spark--two">+</div>
                    </div>
                    <div className="sharebizz-build__caption"><span /> Making progress, one detail at a time</div>
                </div>
            </section>

            <footer className="sharebizz-maintenance__footer">
                <span>ShareBizz</span>
                <span>Something worth sharing is taking shape.</span>
            </footer>
        </main>
    )
}